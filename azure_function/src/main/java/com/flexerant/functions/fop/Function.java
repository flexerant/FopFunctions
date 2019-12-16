package com.flexerant.functions.fop;

import java.io.BufferedOutputStream;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.security.InvalidKeyException;
import java.security.NoSuchAlgorithmException;
import java.util.logging.Level;

import javax.xml.transform.Result;
import javax.xml.transform.Source;
import javax.xml.transform.Transformer;
import javax.xml.transform.TransformerFactory;
import javax.xml.transform.sax.SAXResult;
import javax.xml.transform.stream.StreamSource;

import com.microsoft.azure.functions.annotation.*;

import org.apache.commons.codec.digest.HmacUtils;
import org.apache.commons.io.IOUtils;
import org.apache.fop.apps.FOUserAgent;
import org.apache.fop.apps.Fop;
import org.apache.fop.apps.FopFactory;
import org.apache.fop.apps.FormattingResults;
import org.apache.fop.apps.MimeConstants;
import org.apache.fop.apps.PageSequenceResults;

import com.microsoft.azure.functions.*;

/**
 * Azure Functions with HTTP Trigger.
 */
public class Function {

    private static FopFactory fopFactory = null;

    /**
     * This function listens at endpoint "/api/HttpTrigger-Java". Two ways to invoke
     * it using "curl" command in bash: 1. curl -d "HTTP Body" {your
     * host}/api/HttpTrigger-Java&code={your function key} 2. curl "{your
     * host}/api/HttpTrigger-Java?name=HTTP%20Query&code={your function key}"
     * Function Key is not needed when running locally, it is used to invoke
     * function deployed to Azure. More details:
     * https://aka.ms/functions_authorization_keys
     */
    @FunctionName("Fop")
    public HttpResponseMessage run(@HttpTrigger(name = "req", methods = { HttpMethod.GET,
            HttpMethod.POST }, authLevel = AuthorizationLevel.ANONYMOUS, dataType = "binary") HttpRequestMessage<byte[]> request,
            final ExecutionContext context) {

        context.getLogger().info("Java HTTP trigger processed a request.");

        File foTemp = null;
        File outputPath = null;

        try {

            if (fopFactory == null) {

                File xconf = new File(System.getenv("FOP_CONFIG_PATH"));
                File fontCache = new File(System.getenv("FOP_FONT_CACHE_PATH"));

                fopFactory = FopFactory.newInstance(xconf);
                fopFactory.getFontManager().setCacheFile(fontCache.toURI());
            }

            foTemp = File.createTempFile("foinput", ".tmp");
            FileOutputStream foOut = new FileOutputStream(foTemp);

            foOut.write(request.getBody());
            foOut.close();

            String signature = this.calcShaHash(foTemp, System.getenv("PASSWORD_KEY"), context);
            String sig = request.getQueryParameters().get("sig");

            context.getLogger().info("Signature: " + signature);
            context.getLogger().info("sig: " + sig);

            if (!signature.equals(sig)) {
                context.getLogger().log(Level.SEVERE, "The signatures do not match. Rejecting request.");
                return request.createResponseBuilder(HttpStatus.FORBIDDEN).build();
            }

            outputPath = File.createTempFile("fop", ".tmp");

            FOUserAgent foUserAgent = fopFactory.newFOUserAgent();

            // Setup output stream. Note: Using BufferedOutputStream
            // for performance reasons (helpful with FileOutputStreams).
            FileOutputStream outStream = new FileOutputStream(outputPath);
            BufferedOutputStream out = new BufferedOutputStream(outStream);

            // Construct fop with desired output format
            Fop fop = fopFactory.newFop(MimeConstants.MIME_PDF, foUserAgent, out);

            // Setup JAXP using identity transformer
            TransformerFactory factory = TransformerFactory.newInstance();
            Transformer transformer = factory.newTransformer(); // identity transformer

            // Setup input stream
            Source src = new StreamSource(new FileInputStream(foTemp));

            // Resulting SAX events (the generated FO) must be piped through to FOP
            Result res = new SAXResult(fop.getDefaultHandler());

            // Start XSLT transformation and FOP processing
            transformer.transform(src, res);

            // Result processing
            FormattingResults foResults = fop.getResults();
            java.util.List pageSequences = foResults.getPageSequences();

            for (Object pageSequence : pageSequences) {
                PageSequenceResults pageSequenceResults = (PageSequenceResults) pageSequence;
                context.getLogger()
                        .info("PageSequence " + (String.valueOf(pageSequenceResults.getID()).length() > 0
                                ? pageSequenceResults.getID()
                                : "<no id>") + " generated " + pageSequenceResults.getPageCount() + "pages.");
            }
            context.getLogger().info("Generated " + foResults.getPageCount() + " pages in total.");

            out.close();

            byte[] data = IOUtils.toByteArray(new FileInputStream(outputPath));

            return request.createResponseBuilder(HttpStatus.OK).header("Content-Type", "application/pdf").body(data)
                    .build();

        } catch (Exception ex) {

            context.getLogger().severe(ex.getMessage());
            return request.createResponseBuilder(HttpStatus.INTERNAL_SERVER_ERROR).body(ex.getMessage()).build();
        } finally {
            if (outputPath != null)
                outputPath.deleteOnExit();
            if (foTemp != null)
                foTemp.deleteOnExit();
        }
    }

    private String calcShaHash(File file, String key, ExecutionContext context)
            throws NoSuchAlgorithmException, InvalidKeyException, IOException {

        return HmacUtils.hmacSha1Hex(key.getBytes(), new FileInputStream(file));
    }
}